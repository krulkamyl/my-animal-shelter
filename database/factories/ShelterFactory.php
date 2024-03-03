<?php

namespace Database\Factories;

use App\Models\Shelter;
use Illuminate\Database\Eloquent\Factories\Factory;

class ShelterFactory extends Factory
{
    public static function prepare(): Shelter
    {
        $faker = \Faker\Factory::create();
        $shelter = Shelter::create(
            [
                'name' => $faker->name,
                'description' => $faker->text,
                'capacity' => $faker->numberBetween(1, 1000),
            ]
        );

        $shelter->address()->create(
            [
                'street' => $faker->streetAddress,
                'city' => $faker->city,
                'state' => $faker->state,
                'zip' => $faker->postcode,
            ]
        );

        return $shelter;
    }

    public function definition(): array
    {
        return [
            'name' => $this->faker->name,
            'description' => $this->faker->text,
            'capacity' => $this->faker->numberBetween(1, 1000),
        ];
    }
}
